import { gql } from 'apollo-angular';

export const LOGIN = gql`
  mutation Login($input: LoginCommandInput!) {
    login(input: $input) {
      accessToken
      accessTokenExpiresAtUtc
      refreshToken
      refreshTokenExpiresAtUtc
      user {
        id
        email
        displayName
        roles
        permissions
      }
    }
  }
`;

export const REFRESH_TOKEN = gql`
  mutation RefreshToken($refreshToken: String!) {
    refreshToken(refreshToken: $refreshToken) {
      accessToken
      accessTokenExpiresAtUtc
      refreshToken
      refreshTokenExpiresAtUtc
      user {
        id
        email
        displayName
        roles
        permissions
      }
    }
  }
`;

export const LOGOUT = gql`
  mutation Logout($refreshToken: String!) {
    logout(refreshToken: $refreshToken)
  }
`;

export const ME = gql`
  query Me {
    me {
      id
      email
      displayName
      roles
      permissions
    }
  }
`;

export const SEARCH_EXAMS = gql`
  query SearchExams($criteria: ExamSearchCriteriaInput!) {
    searchExams(criteria: $criteria) {
      totalCount
      page
      pageSize
      items {
        id
        title
        description
        status
        timeLimitMinutes
        passingScorePercentage
        ownerId
        ownerName
        sectionCount
        itemCount
        totalScore
        createdAtUtc
        publishedAtUtc
      }
    }
  }
`;

export const EXAM_BY_ID = gql`
  query ExamById($id: UUID!) {
    examById(id: $id) {
      summary {
        id
        title
        description
        status
        timeLimitMinutes
        passingScorePercentage
        ownerId
        ownerName
        sectionCount
        itemCount
        totalScore
        createdAtUtc
        publishedAtUtc
      }
      compositionViolations
      sections {
        id
        title
        instructions
        position
        items {
          id
          itemId
          position
          scoreOverride
          effectiveScore
          item {
            id
            stem
            type
            status
            difficulty
            maximumScore
            categoryName
          }
        }
      }
    }
  }
`;

export const CREATE_EXAM = gql`
  mutation CreateExam($input: CreateExamCommandInput!) {
    createExam(input: $input)
  }
`;

export const UPDATE_EXAM = gql`
  mutation UpdateExam($input: UpdateExamCommandInput!) {
    updateExam(input: $input)
  }
`;

export const DELETE_EXAM = gql`
  mutation DeleteExam($examId: UUID!) {
    deleteExam(examId: $examId)
  }
`;

export const PUBLISH_EXAM = gql`
  mutation PublishExam($examId: UUID!) {
    publishExam(examId: $examId)
  }
`;

export const ARCHIVE_EXAM = gql`
  mutation ArchiveExam($examId: UUID!) {
    archiveExam(examId: $examId)
  }
`;

export const RETURN_EXAM_TO_DRAFT = gql`
  mutation ReturnExamToDraft($examId: UUID!) {
    returnExamToDraft(examId: $examId)
  }
`;

export const ADD_EXAM_SECTION = gql`
  mutation AddExamSection($input: AddExamSectionCommandInput!) {
    addExamSection(input: $input)
  }
`;

export const UPDATE_EXAM_SECTION = gql`
  mutation UpdateExamSection($input: UpdateExamSectionCommandInput!) {
    updateExamSection(input: $input)
  }
`;

export const REMOVE_EXAM_SECTION = gql`
  mutation RemoveExamSection($input: RemoveExamSectionCommandInput!) {
    removeExamSection(input: $input)
  }
`;

export const REORDER_EXAM_SECTIONS = gql`
  mutation ReorderExamSections($input: ReorderExamSectionsCommandInput!) {
    reorderExamSections(input: $input)
  }
`;

export const ADD_EXAM_ITEM = gql`
  mutation AddExamItem($input: AddExamItemCommandInput!) {
    addExamItem(input: $input)
  }
`;

export const REMOVE_EXAM_ITEM = gql`
  mutation RemoveExamItem($input: RemoveExamItemCommandInput!) {
    removeExamItem(input: $input)
  }
`;

export const REORDER_EXAM_ITEMS = gql`
  mutation ReorderExamItems($input: ReorderExamItemsCommandInput!) {
    reorderExamItems(input: $input)
  }
`;

export const SEARCH_USERS = gql`
  query SearchUsers($criteria: UserSearchCriteriaInput!) {
    searchUsers(criteria: $criteria) {
      totalCount
      page
      pageSize
      items {
        id
        email
        displayName
        isActive
        lastSignInAtUtc
        createdAtUtc
        roles {
          id
          name
          description
          isSystemRole
        }
      }
    }
  }
`;

export const ROLES = gql`
  query Roles {
    roles {
      id
      name
      description
      isSystemRole
      userCount
      permissions {
        id
        name
        description
      }
    }
  }
`;

export const PERMISSIONS = gql`
  query PermissionCatalogue {
    permissions {
      id
      name
      description
    }
  }
`;

export const CREATE_USER = gql`
  mutation CreateUser($input: CreateUserCommandInput!) {
    createUser(input: $input)
  }
`;

export const UPDATE_USER = gql`
  mutation UpdateUser($input: UpdateUserCommandInput!) {
    updateUser(input: $input)
  }
`;

export const SET_USER_ACTIVE = gql`
  mutation SetUserActive($input: SetUserActiveCommandInput!) {
    setUserActive(input: $input)
  }
`;

export const CREATE_ROLE = gql`
  mutation CreateRole($input: CreateRoleCommandInput!) {
    createRole(input: $input)
  }
`;

export const UPDATE_ROLE = gql`
  mutation UpdateRole($input: UpdateRoleCommandInput!) {
    updateRole(input: $input)
  }
`;

export const DELETE_ROLE = gql`
  mutation DeleteRole($roleId: UUID!) {
    deleteRole(roleId: $roleId)
  }
`;
